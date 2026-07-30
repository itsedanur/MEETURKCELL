const fs = require('fs');
const glob = require('glob');

const files = [
  'src/layouts/AuthLayout.tsx',
  'src/layouts/Header.tsx',
  'src/features/auth/components/Login.tsx',
  'src/features/meetings/components/MeetingCreate.tsx',
  'src/features/meetings/components/MeetingDetail.tsx',
  'src/features/meetings/components/MeetingList.tsx',
  'src/pages/Dashboard.tsx',
  'src/pages/NotFound.tsx',
  'src/routes/ProtectedRoute.tsx'
];

files.forEach(file => {
  if (!fs.existsSync(file)) return;
  let content = fs.readFileSync(file, 'utf8');

  // Fix Box system props
  content = content.replace(/<Box ([^>]+)>/g, (match, props) => {
    if (props.includes('sx=')) return match;
    const sxProps = [];
    let newProps = props;

    ['display', 'justifyContent', 'alignItems', 'flexDirection', 'textAlign', 'maxWidth', 'mx', 'mt', 'mb', 'p', 'pt', 'pb'].forEach(prop => {
      const regex = new RegExp(`${prop}="([^"]+)"`);
      if (regex.test(newProps)) {
        const val = newProps.match(regex)[1];
        sxProps.push(`${prop}: '${val}'`);
        newProps = newProps.replace(regex, '');
      }
      
      const numRegex = new RegExp(`${prop}={([^}]+)}`);
      if (numRegex.test(newProps)) {
        const val = newProps.match(numRegex)[1];
        sxProps.push(`${prop}: ${val}`);
        newProps = newProps.replace(numRegex, '');
      }
    });

    if (sxProps.length > 0) {
      return `<Box sx={{ ${sxProps.join(', ')} }} ${newProps}>`;
    }
    return match;
  });

  // Fix Grid item
  content = content.replace(/<Grid item/g, '<Grid');

  // Fix Typography mt mb
  content = content.replace(/<Typography ([^>]+)>/g, (match, props) => {
    if (props.includes('sx=')) return match;
    const sxProps = [];
    let newProps = props;

    ['mt', 'mb', 'textAlign', 'fontWeight'].forEach(prop => {
      const numRegex = new RegExp(`${prop}={([^}]+)}`);
      if (numRegex.test(newProps)) {
        const val = newProps.match(numRegex)[1];
        sxProps.push(`${prop}: ${val}`);
        newProps = newProps.replace(numRegex, '');
      }
      
      const regex = new RegExp(`${prop}="([^"]+)"`);
      if (regex.test(newProps)) {
        const val = newProps.match(regex)[1];
        sxProps.push(`${prop}: '${val}'`);
        newProps = newProps.replace(regex, '');
      }
    });

    if (sxProps.length > 0) {
      return `<Typography sx={{ ${sxProps.join(', ')} }} ${newProps}>`;
    }
    return match;
  });

  fs.writeFileSync(file, content);
});
